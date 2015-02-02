
/*
 * 删除数组中的对象
 */
Array.prototype.remove = function (e) {
    if (typeof (e) == "number") {
        this.splice(e, 1);
    }
}

/*
 * 添加到数组中
 */
Array.prototype.add = function (e) {
    this[this.length] = e;
}

Array.prototype.contains = function (e) {
    for (var key in this) {
        if (this[key].Text == e) {
            return true;
        }
    }
    return false;
}

/*
 * 认证对象
 */
var Verify = {
    currentUrl: window.location.origin + window.location.pathname,
    Sender: undefined,
    Db: {
        VerifyType: {
            url: window.location.origin + window.location.pathname,
            // 删除认证类型
            del: function (e) {
                var srcId = $(e).attr("id");
                if (srcId && srcId != "") {
                    Verify.get(e, this.url, { action: "del", id: srcId, fn: "Verify.Db.VerifyType.delCallback" });
                }
            },
            add: function (e) {
                if ($("#name").val() == "") {
                    error("名称不能为空！");
                    return;
                }
                Verify.get(e, this.url, { action: "add", name: $("#name").val(), comments: $("#comments").val(), fn: "Verify.Db.VerifyType.addCallback" });
            },
            update: function (e) {
                var id = $(e).attr("typeId");
                if (id && id != "") {

                    if ($("#name").val == "") {
                        error("名称不能为空！");
                        return;
                    }

                    Verify.get(e, this.url, { action: "update", id: id, name: $("#name").val(), comments: $("#comments").val(), fn: "Verify.Db.VerifyType.updateCallback" });
                }

            },
            // 删除回调
            delCallback: function (e) {
                showMsg(e, function () {
                    $(Verify.Sender).parents("tr").remove();
                });
            },
            addCallback: function (e) {
                showMsg(e, function () {
                    window.location.href = Verify.Db.VerifyType.url;
                });
            },
            updateCallback: function (e) {
                this.addCallback(e);
            }
        }
    },

    // 认证模板
    Template: {
        typeId: "", // 当前编辑模板的认证类型
        src: new Array(), // 待添加的模板
        /*
         * 添加一项到下拉列表
         * @return 返回格式：{text:"",value:""}
         */
        addItem: function (e, text, val) {
            var option = $("<option value='" + val + "' selected='selected'>" + text + "</option>");
            $(e).children("option").each(function (index, value) {
                if ($(value).val() == "") {
                    $(value).remove();
                }
                if ($(value).attr("selected")) {
                    $(value).removeAttr("selected");
                }
            });
            $(e).append(option);
            return { text: text, value: val };
        },

        delItem: function (e) {
            // 删除html
            var tr = $(e).parents("tr");
            var index = tr.attr("index");
            tr.remove();
            if (index) {
                // 删除对象
                var i = parseInt(index);
                this.src.remove(i);
            }
        },

        addToEditView: function (e, m) {
            if (!m) {
                m = new VerifyTemplate().init();
            }
            if (m.Text == "") {
                error("名称必填", "请填写名称后再添加！");
                return;
            }

            if (this.src.contains(m.Text)) {
                error("名称已经存在！", "请更改一下名称！");
                return;
            }

            // 下拉列表为空
            if (m.DataType == 3 && $("#ddl option").length <= 0) {
                return;
            }
            if (m.DataType == 3 && $("#ddl option:eq(0)").val() == "") {
                return;
            }

            var tr = this.getTemplate(m);

            tr.attr("index", this.src.length);

            $("#name").val("");

            // 制作模板
            $(e).append(tr);

            // 添加到全局中
            this.src.add(m);
        },

        /*
         * 保存模板到服务器中
         */
        save: function () {
            if (this.src.length <= 0) {
                error("模板为空！", "添加点东西再保存呗~~");
                return;
            }
            try {
                var json = $.toJSON(this.src);
                $.post(Verify.currentUrl, { action: "add", value: json, typeId: this.typeId, fn: "Verify.Template.saveCallback" }); // 提交Json
            } catch (e) {
                error("JSON解析错误！", "请添加Jquery.json.js 的引用！");
            }

        },

        saveCallback: function (e) {
            showMsg(e);
        },

        /*
         * 初始化模板
         * @param e 模板列表的 Json字符串
         */
        init: function (t, e) {
            var obj = $.parseJSON(e);
            for (var i = 0; i < obj.length; i++) {
                var m = new VerifyTemplate().convert(obj[i]);
                this.src.add(m);

                var tr = this.getTemplate(m);
                tr.attr("index", i);

                // 下拉列表处理
                if (m.DataType == 3) {

                    var select = tr.find(".apply-textbox");
                    select.children("option").remove();

                    for (var j = 0; j < m.Items.length; j++) {
                        var item = m.Items[j];
                        var option = $("<option></option>").val(item.Value).text(item.Text);
                        select.append(option);
                    }

                }

                $(t).append(tr);
            }
        },

        /*
         * 获取模板
         * @param m  VerifyTemplate类
         * @return  tr html
         */
        getTemplate: function (m) {
            var tr = $("<tr></tr>");
            var th = $("<th></th>");
            if (m.Required) {
                th.append($("<span>*</span>"));
            }

            // 类型处理
            var tdType = $("<td></td>");
            if (m.DataType == 1) {
                tdType.html("<textarea class='apply-textbox' id='" + m.Name + "'></textarea>");
            } else if (m.DataType == 2) {
                tdType.html("<input type='file' class='apply-textbox' maxlength='" + m.MaxLen + "' value='' id='" + m.Name + "' name='" + m.Name + "' />");
            } else if (m.DataType == 3) {
                var selector = $("#ddl");
                tdType.append($("<select class='apply-textbox'></select>").append(selector.children("option")));
            }
            else {
                tdType.html("<input type='text' class='apply-textbox' maxlength='" + m.MaxLen + "' value='' id='" + m.Name + "' name='" + m.Name + "' />");
            }


            th.append(m.Text);
            tr.append(th);
            tr.append(tdType);
            tr.append($("<td></td>").append($("<span class='tips'></span>").text(m.Tips)));
            tr.append($("<td><a href='javascript:void(0)' onclick='Verify.Template.delItem(this)' id='" + m.Text + "'>删除</a><td>"));
            return tr;
        }
    },

    get: function (e, url, params) {
        Verify.Sender = e;
        $.get(url, params);
    }

};


/*
 * 模板类
 */
var VerifyTemplate = function () {
    this.Mid = ""; //主键Id
    this.Text = ""; //显示名称
    this.Name = ""; //控件Id
    this.DataType = 0; // 默认为文本类型
    this.MaxLen = 128;
    this.Required = false;
    this.Tips = undefined;
    this.Items = undefined; // 下来选择项

    this.init = function () {
        this.Text = $("#name").val();
        this.DataType = $("#ctrlType").val();
        this.MaxLen = $("#maxLength").val();
        this.Required = $("#requried").is(':checked');
        this.Tips = $("#tipsMsg").val();
        this.Items = this.getItems("#ddl");
        return this;
    };

    // Json对象转换为该对象
    this.convert = function (e) {
        this.Mid = e.Mid;
        this.Text = e.Text;
        this.DataType = e.DataType;
        this.MaxLen = e.MaxLen;
        this.Required = e.Required;
        this.Tips = e.Tips;
        this.Items = e.Items;
        return this;
    };

    // 获取下拉列表的对象。
    this.getItems = function (e) {
        var ddlValues = [];

        // 下拉列表遍历
        $(e).children("option").each(function (index, value) {
            var val = $(value).val();
            if (val == "") {
                return;
            }
            ddlValues[ddlValues.length] = { Text: $(value).text(), Value: val };
        });

        return ddlValues;
    }
}

function success(title, msg) {
    swal({
        title: title,
        text: msg,
        timer: 2000,
        type: "success"
    });
}

function error(title, msg) {
    swal({
        title: title,
        text: msg,
        timer: 2000,
        type: "error"
    });
}

function showMsg(e, fn) {
    if (e.code == 1) {
        success(e.message);
        if (fn) {
            fn();
        }
    } else {
        error(e.message);
    }
    Verify.Sender = undefined;
}