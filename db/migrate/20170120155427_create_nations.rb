class CreateNations < ActiveRecord::Migration[5.0]
  def change
    create_table :nations do |t|
      t.string :name

      t.timestamps
    end
	
	change_column :nations, :id, :string
  end
end
